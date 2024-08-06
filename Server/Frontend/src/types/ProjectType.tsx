export enum ProjectType {
  SEECity = "SEECity",
  DiffCity = "DiffCity",
  SEECityEvolution = "SEECityEvolution",
  SEEJlgCity = "SEEJlgCity",
  SEEReflexionCity = "SEEReflexionCity",
}

export class ProjectTypeUtils {
  static getAll(): ProjectType[] {
    return Object.values(ProjectType).filter((value) => typeof value === 'string').map((value) => value as ProjectType);
  }

  static getFileExtension(ft: ProjectType): string {
    switch (ft) {
      case ProjectType.SEECity: return ".zip";
      case ProjectType.DiffCity: return ".zip";
      case ProjectType.SEECityEvolution: return ".zip";
      case ProjectType.SEEJlgCity: return ".zip";
      case ProjectType.SEEReflexionCity: return ".zip";
      default: return "*";
    }
  }

  static getLabel(ft: ProjectType): string {
    switch (ft) {
      case ProjectType.DiffCity: return "Diff City";
      case ProjectType.SEECity: return "Code City";
      case ProjectType.SEECityEvolution: return "City Evolution";
      case ProjectType.SEEJlgCity: return "JLG City";
      case ProjectType.SEEReflexionCity: return "Reflexion City";
      default: return "Unspecified";
    }
  }
}

export default ProjectType;
