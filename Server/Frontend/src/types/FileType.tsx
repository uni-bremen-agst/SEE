enum FileType {
  CFG = "CFG",
  CSV = "CSV",
  GXL = "GXL",
  SOURCE = "SOURCE",
  SOLUTION = "SOLUTION",
}

namespace FileType {
  export function asList() : FileType[] {
    return Object.values(FileType).filter((value) => typeof value === 'string').map((value) => value as FileType);
  }

  export function getFileExtension(ft: FileType) : string {
    switch (ft) {
      case FileType.CFG: return ".cfg";
      case FileType.CSV: return ".csv";
      case FileType.GXL: return ".gxl";
      case FileType.SOURCE: return ".zip";
      case FileType.SOLUTION: return "*";
      default: return "*";
    }
  }

  export function getLabel(ft: FileType) : string {
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

export default FileType