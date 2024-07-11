import FileType from "../types/FileType";

type SeeFile = {
    id: string;
    name: string;
    contentType: string;
    fileType: FileType;
    size: number;
    creationTime: number;
    _localfile: File;
}

export default SeeFile