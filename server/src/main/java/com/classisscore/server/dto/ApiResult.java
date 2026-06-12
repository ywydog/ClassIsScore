package com.classisscore.server.dto;

import lombok.Data;
import lombok.Builder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ApiResult<T> {

    private Integer code;
    private String message;
    private T data;

    public static <T> ApiResult<T> success(T data) {
        return ApiResult.<T>builder()
                .code(0)
                .message("success")
                .data(data)
                .build();
    }

    public static <T> ApiResult<T> success() {
        return ApiResult.<T>builder()
                .code(0)
                .message("success")
                .build();
    }

    public static <T> ApiResult<T> error(String message) {
        return ApiResult.<T>builder()
                .code(-1)
                .message(message)
                .build();
    }

    public static <T> ApiResult<T> error(Integer code, String message) {
        return ApiResult.<T>builder()
                .code(code)
                .message(message)
                .build();
    }
}
