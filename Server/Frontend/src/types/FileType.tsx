export enum FileType {
  CFG = "CFG",
  CSV = "CSV",
  GXL = "GXL",
  SOURCE = "SOURCE",
  SOLUTION = "SOLUTION",
}

export class FileTypeUtils {
  static getAll() : FileType[] {
    return Object.values(FileType).filter((value) => typeof value === 'string').map((value) => value as FileType);
  }

  static getFileExtension(ft: FileType) : string {
    switch (ft) {
      case FileType.CFG: return ".cfg";
      case FileType.CSV: return ".csv";
      case FileType.GXL: return ".gxl";
      case FileType.SOURCE: return ".zip";
      case FileType.SOLUTION: return "*";
      default: return "*";
    }
  }

  static getLabel(ft: FileType) : string {
    switch (ft) {
      case FileType.CFG: return "Configuration (CFG)";
      case FileType.CSV: return "CSV Data Provider";
      case FileType.GXL: return "GLX Data Provider";
      case FileType.SOURCE: return "Source Code Archive (ZIP)";
      case FileType.SOLUTION: return "Visual Studio Solution";
      default: return "Unspecified";
    }
  }
}

export default FileType;
// export const FileTypeModule = { FileType, FileTypeUtils };