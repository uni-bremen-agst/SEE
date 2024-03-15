package uni.bachelorprojekt.see.controller.file.payload;

import lombok.Data;

import java.time.ZonedDateTime;

/**
 * Copyright 2021 LFS-Software UG, all rights reserved.
 *
 * @author Thorsten Friedewold (friedewold@lfs-software.de)
 */
@Data
public class PayloadFile {

    private String id;
    private ZonedDateTime creationTime;
    private String originalFileName;
    private String contentType;
    private byte[] content;

}
