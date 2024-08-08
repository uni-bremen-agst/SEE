import ProjectType from "../types/ProjectType";

type SeeFile = {
  id: string;
  name: string;
  contentType: string;
  projectType: ProjectType;
  size: number;
  creationTime: number;
  _localfile: File;
}

export default SeeFile
