package uni.bachelorprojekt.see.service;

import io.minio.GetObjectArgs;
import io.minio.MinioClient;
import io.minio.PutObjectArgs;
import io.minio.RemoveObjectArgs;
import io.minio.errors.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.io.IOUtils;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import uni.bachelorprojekt.see.Util.FileType;
import uni.bachelorprojekt.see.controller.file.payload.PayloadFile;
import uni.bachelorprojekt.see.model.File;
import uni.bachelorprojekt.see.model.Server;
import uni.bachelorprojekt.see.repo.FileRepo;

import java.io.IOException;
import java.io.InputStream;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Service
@Transactional
@RequiredArgsConstructor
@Slf4j
public class FileService {

    private final FileRepo fileRepo;

    @Value("${see.app.minio.url}")
    private String url;

    @Value("${see.app.minio.bucket}")
    private String bucket;

    @Value("${see.app.minio.user}")
    private String user;

    @Value("${see.app.minio.password}")
    private String password;

    private MinioClient createClient() {
        return MinioClient.builder()
                .endpoint(url)
                .credentials(user, password)
                .build();
    }

    public File createFile(MultipartFile multipartFile) {
        if (multipartFile.isEmpty()) {
            return null;
        }
        File file = new File();
        file.setContentType(multipartFile.getContentType());
        file.setOriginalFileName(multipartFile.getOriginalFilename());
        file.setPath(String.valueOf(file.getId()));
        fileRepo.save(file);

        try {
            upload(file.getId().toString(), multipartFile);
        } catch (Exception e) {
            throw new IllegalStateException("The file cannot be upload on the internal storage. Please retry later", e);
        }
        return file;
    }

    public void upload(String name, MultipartFile file) throws IOException, ServerException, InsufficientDataException, ErrorResponseException, NoSuchAlgorithmException, InvalidKeyException, InvalidResponseException, XmlParserException, InternalException {
        MinioClient minioClient = createClient();

        minioClient.putObject(PutObjectArgs
                .builder()
                .bucket(bucket)
                .object(name)
                .stream(file.getInputStream(), file.getSize(), -1)
                .build());
    }


    public PayloadFile getFile(UUID fileID) throws ServerException, InsufficientDataException, ErrorResponseException, IOException, NoSuchAlgorithmException, InvalidKeyException, InvalidResponseException, XmlParserException, InternalException {
        PayloadFile payloadFile = new PayloadFile();
        Optional<File> file = fileRepo.findById(fileID);
        if (file.isEmpty()) {
            log.error("Cant fetch file {}", fileID);
            return null;
        }
        MinioClient minioClient = createClient();

        InputStream stream =
                minioClient.getObject(
                        GetObjectArgs
                                .builder()
                                .bucket("test")
                                .object(fileID.toString())
                                .build());

        payloadFile.setContent(IOUtils.toByteArray(stream));
        payloadFile.setId(fileID.toString());
        payloadFile.setOriginalFileName(file.get().getOriginalFileName());
        payloadFile.setContentType(file.get().getContentType());
        payloadFile.setCreationTime(file.get().getCreationTime());

        log.info("Fetching file {}", fileID);
        return payloadFile;
    }

    public PayloadFile getFileByServerAndFileType(Server server, FileType fileType) throws ServerException, InsufficientDataException, ErrorResponseException, IOException, NoSuchAlgorithmException, InvalidKeyException, InvalidResponseException, XmlParserException, InternalException {
        PayloadFile payloadFile = new PayloadFile();
        if (server == null || fileType == null){
            return null;
        }
        Optional<File> file = fileRepo.findFileByServerAndFileType(server, fileType);
        if (file.isEmpty()) {
            log.error("Cant fetch file for server {} with type {}", server.getId(), fileType);
            return null;
        }
        MinioClient minioClient = createClient();

        InputStream stream =
                minioClient.getObject(
                        GetObjectArgs
                                .builder()
                                .bucket("test")
                                .object(file.get().getId().toString())
                                .build());

        payloadFile.setContent(IOUtils.toByteArray(stream));
        payloadFile.setId(file.get().getId().toString());


        payloadFile.setOriginalFileName(file.get().getOriginalFileName());

        switch (fileType) {
            case CSV -> payloadFile.setOriginalFileName("multiplayer.csv");
            case CONFIG -> payloadFile.setOriginalFileName("multiplayer.cfg");
            case GXL -> payloadFile.setOriginalFileName("multiplayer.gxl");
            case SOLUTION ->
                    payloadFile.setOriginalFileName("solution." + file.get().getOriginalFileName().split("\\.")[file.get().getOriginalFileName().split("\\.").length - 1]);
            case SOURCE -> payloadFile.setOriginalFileName("src.zip");
        }

        payloadFile.setContentType(file.get().getContentType());
        payloadFile.setCreationTime(file.get().getCreationTime());

        log.info("Fetching file {}", file.get().getId().toString());
        return payloadFile;
    }

    public boolean deleteFile(UUID fileID) throws ServerException, InsufficientDataException, ErrorResponseException, IOException, NoSuchAlgorithmException, InvalidKeyException, InvalidResponseException, XmlParserException, InternalException {

        Optional<File> file = fileRepo.findById(fileID);
        if (file.isEmpty()) {
            log.error("Cant remove file {}", fileID);
            return false;
        }

        MinioClient minioClient = createClient();

        minioClient.removeObject(
                RemoveObjectArgs
                        .builder()
                        .bucket(bucket)
                        .object(fileID.toString())
                        .build());

        fileRepo.delete(file.get());
        log.info("Removing file {}", fileID);
        return true;
    }


    public List<File> getFilesByServer(Server server) {
        return fileRepo.findFilesByServer(server);
    }

    public void deleteFilesByServer(Server server) {
        List<File> files = getFilesByServer(server);

        for (File file : files) {
            try {
                deleteFile(file.getId());
            } catch (Exception e) {
                log.error("Cant delete file {}", file.getId());
            }
        }

    }
}
