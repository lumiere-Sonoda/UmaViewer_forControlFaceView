#import <Cocoa/Cocoa.h>
#import <dispatch/dispatch.h>
#import <stdbool.h>
#import <stdlib.h>
#import <string.h>

static const char kPathSeparator = 0x1C;

typedef void (*DialogOpenCallback)(const char *paths);
typedef void (*DialogSaveCallback)(const char *path);
typedef char * (^DialogOperation)(void);

static NSString *ToNSString(const char *value) {
    if (value == NULL) {
        return @"";
    }

    NSString *result = [NSString stringWithUTF8String:value];
    return result != nil ? result : @"";
}

static char *ToCString(NSString *value) {
    const char *utf8 = [value UTF8String];
    if (utf8 == NULL) {
        return NULL;
    }

    size_t length = strlen(utf8);
    char *copy = (char *)malloc(length + 1);
    if (copy == NULL) {
        return NULL;
    }

    memcpy(copy, utf8, length + 1);
    return copy;
}

static NSURL *DirectoryURLFromPath(const char *directory) {
    NSString *path = ToNSString(directory);
    if (path.length == 0) {
        return nil;
    }

    BOOL isDirectory = NO;
    if ([[NSFileManager defaultManager] fileExistsAtPath:path isDirectory:&isDirectory] && isDirectory) {
        return [NSURL fileURLWithPath:path isDirectory:YES];
    }

    return nil;
}

static NSString *SanitizeExtension(NSString *raw) {
    NSString *value = [raw stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
    while (value.length > 0) {
        unichar first = [value characterAtIndex:0];
        if (first == '.' || first == '*') {
            value = [value substringFromIndex:1];
            continue;
        }

        break;
    }

    if (value.length == 0 || [value isEqualToString:@"*"]) {
        return @"";
    }

    return [value lowercaseString];
}

static NSArray<NSString *> *ParseExtensions(const char *filters) {
    NSString *filterString = ToNSString(filters);
    if (filterString.length == 0) {
        return @[];
    }

    NSMutableOrderedSet<NSString *> *extensions = [NSMutableOrderedSet orderedSet];
    NSArray<NSString *> *groups = [filterString componentsSeparatedByString:@";"];

    for (NSString *group in groups) {
        if (group.length == 0) {
            continue;
        }

        NSArray<NSString *> *parts = [group componentsSeparatedByString:@"|"];
        NSString *extensionGroup = parts.count > 1 ? parts[1] : parts[0];
        NSArray<NSString *> *tokens = [extensionGroup componentsSeparatedByString:@","];
        for (NSString *token in tokens) {
            NSString *sanitized = SanitizeExtension(token);
            if (sanitized.length > 0) {
                [extensions addObject:sanitized];
            }
        }
    }

    return [extensions array];
}

static NSString *JoinPathsFromURLs(NSArray<NSURL *> *urls) {
    if (urls.count == 0) {
        return @"";
    }

    NSMutableArray<NSString *> *paths = [NSMutableArray arrayWithCapacity:urls.count];
    for (NSURL *url in urls) {
        if (url.path.length > 0) {
            [paths addObject:url.path];
        }
    }

    NSString *separator = [NSString stringWithFormat:@"%c", kPathSeparator];
    return [paths componentsJoinedByString:separator];
}

static char *RunDialog(DialogOperation operation) {
    if ([NSThread isMainThread]) {
        return operation();
    }

    __block char *result = NULL;
    dispatch_sync(dispatch_get_main_queue(), ^{
        result = operation();
    });
    return result;
}

#ifdef __cplusplus
extern "C" {
#endif

const char *DialogOpenFilePanel(const char *title, const char *directory, const char *filters, int multiselect) {
    return RunDialog(^char *{
        NSOpenPanel *panel = [NSOpenPanel openPanel];
        panel.canChooseFiles = YES;
        panel.canChooseDirectories = NO;
        panel.allowsMultipleSelection = multiselect != 0;
        panel.resolvesAliases = YES;
        panel.treatsFilePackagesAsDirectories = NO;

        NSArray<NSString *> *extensions = ParseExtensions(filters);
        if (extensions.count > 0) {
            panel.allowedFileTypes = extensions;
        }

        NSURL *directoryURL = DirectoryURLFromPath(directory);
        if (directoryURL != nil) {
            panel.directoryURL = directoryURL;
        }

        NSString *panelTitle = ToNSString(title);
        if (panelTitle.length > 0) {
            panel.title = panelTitle;
        }

        NSInteger result = [panel runModal];
        if (result == NSModalResponseOK) {
            return ToCString(JoinPathsFromURLs(panel.URLs));
        }

        return ToCString(@"");
    });
}

void DialogOpenFilePanelAsync(const char *title, const char *directory, const char *filters, int multiselect, DialogOpenCallback callback) {
    const char *result = DialogOpenFilePanel(title, directory, filters, multiselect);
    if (callback != NULL) {
        callback(result);
    }
}

const char *DialogOpenFolderPanel(const char *title, const char *directory, int multiselect) {
    return RunDialog(^char *{
        NSOpenPanel *panel = [NSOpenPanel openPanel];
        panel.canChooseFiles = NO;
        panel.canChooseDirectories = YES;
        panel.canCreateDirectories = YES;
        panel.allowsMultipleSelection = multiselect != 0;
        panel.resolvesAliases = YES;
        panel.treatsFilePackagesAsDirectories = YES;

        NSURL *directoryURL = DirectoryURLFromPath(directory);
        if (directoryURL != nil) {
            panel.directoryURL = directoryURL;
        }

        NSString *panelTitle = ToNSString(title);
        if (panelTitle.length > 0) {
            panel.title = panelTitle;
        }

        NSInteger result = [panel runModal];
        if (result == NSModalResponseOK) {
            return ToCString(JoinPathsFromURLs(panel.URLs));
        }

        return ToCString(@"");
    });
}

void DialogOpenFolderPanelAsync(const char *title, const char *directory, int multiselect, DialogOpenCallback callback) {
    const char *result = DialogOpenFolderPanel(title, directory, multiselect);
    if (callback != NULL) {
        callback(result);
    }
}

const char *DialogSaveFilePanel(const char *title, const char *directory, const char *defaultName, const char *filters) {
    return RunDialog(^char *{
        NSSavePanel *panel = [NSSavePanel savePanel];
        panel.canCreateDirectories = YES;
        panel.extensionHidden = NO;

        NSArray<NSString *> *extensions = ParseExtensions(filters);
        if (extensions.count > 0) {
            panel.allowedFileTypes = extensions;
        }

        NSURL *directoryURL = DirectoryURLFromPath(directory);
        if (directoryURL != nil) {
            panel.directoryURL = directoryURL;
        }

        NSString *panelTitle = ToNSString(title);
        if (panelTitle.length > 0) {
            panel.title = panelTitle;
        }

        NSString *defaultFileName = ToNSString(defaultName);
        if (defaultFileName.length > 0) {
            panel.nameFieldStringValue = defaultFileName;
        }

        NSInteger result = [panel runModal];
        if (result == NSModalResponseOK && panel.URL.path.length > 0) {
            return ToCString(panel.URL.path);
        }

        return ToCString(@"");
    });
}

void DialogSaveFilePanelAsync(const char *title, const char *directory, const char *defaultName, const char *filters, DialogSaveCallback callback) {
    const char *result = DialogSaveFilePanel(title, directory, defaultName, filters);
    if (callback != NULL) {
        callback(result);
    }
}

#ifdef __cplusplus
}
#endif
