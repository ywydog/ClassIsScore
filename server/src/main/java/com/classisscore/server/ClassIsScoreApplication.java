package com.classisscore.server;

import org.mybatis.spring.annotation.MapperScan;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
@MapperScan("com.classisscore.server.mapper")
public class ClassIsScoreApplication {

    public static void main(String[] args) {
        SpringApplication.run(ClassIsScoreApplication.class, args);
    }
}
