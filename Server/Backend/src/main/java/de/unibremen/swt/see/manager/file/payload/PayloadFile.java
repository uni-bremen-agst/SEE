package de.unibremen.swt.see.manager.file.payload;

import lombok.Data;

import java.time.ZonedDateTime;


@Data
public class PayloadFile {

    private String id;
    private ZonedDateTime creationTime;
    private String originalFileName;
    private String contentType;
    private byte[] content;

}
