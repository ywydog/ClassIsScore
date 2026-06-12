package com.classisscore.server.plugin;

import lombok.Data;
import lombok.Builder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class PluginManifest {

    private Long id;
    private String name;
    private String version;
    private String description;
    private String author;
    private String mainClass;
    private Boolean enabled;
    private String filePath;
}
