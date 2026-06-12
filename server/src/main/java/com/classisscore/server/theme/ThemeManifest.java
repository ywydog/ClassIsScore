package com.classisscore.server.theme;

import lombok.Data;
import lombok.Builder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ThemeManifest {

    private Long id;
    private String name;
    private String version;
    private String description;
    private String author;
    private Boolean enabled;
    private String filePath;
}
