import { Box, Button, Card, CardActionArea, CardContent, Container, IconButton, Modal, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { grey } from "@mui/material/colors";
import { useNavigate } from "react-router";
import Avatar from "../components/Avatar";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faRemove, faRepeat, faX } from "@fortawesome/free-solid-svg-icons";
import { useContext, useState } from "react";
import { MuiFileInput } from 'mui-file-input';
import { AuthContext } from "../contexts/AuthContext";
import ProjectType, { ProjectTypeUtils } from "../types/ProjectType";
import SeeFile from "../types/SeeFile";
import AppUtils from "../utils/AppUtils";

const modalStyle = {
  position: 'absolute',
  top: '50%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  maxWidth: "1200px",
  minWidth: "400px",
  bgcolor: 'background.paper',
  borderRadius: "25px",
  boxShadow: 24,
  p: 4,
};

function getRandomColor() {
  const red = (Math.floor(Math.random() * 150) + 100).toString();
  const green = (Math.floor(Math.random() * 150) + 100).toString();
  const blue = (Math.floor(Math.random() * 150) + 100).toString();

  return `rgb(${red}, ${green}, ${blue})`;
}

function getRandomSeed() {
  let avatarSeed = "";
  for (let i = 0; i < 18; i++) {
    avatarSeed = avatarSeed + Math.round(Math.random()).toString();
  }
  return avatarSeed;
}

function CreateServerView() {
  const { axiosInstance } = useContext(AuthContext);

  const navigate = useNavigate();

  const [fileTypeModalOpen, setFileTypeModalOpen] = useState(false);

  const [name, setName] = useState<string>("");
  const [files, setFiles] = useState<SeeFile[]>([]);

  const [avatarSeed, setAvatarSeed] = useState(getRandomSeed());
  const [avatarColor, setAvatarColor] = useState(getRandomColor());
  const [displayReloadIcon, setDisplayReloadIcon] = useState(false);

  const [errors, setErrors] = useState(new Map<string, string>());

  type ConditionalProjectType = [ProjectType, boolean];
  const [availableProjectTypes, setAvailableProjectTypes] = useState<ConditionalProjectType[]>(ProjectTypeUtils.getAll().map(projectType => [projectType, true]));


  function addFile(projectType: ProjectType) {
    setAvailableProjectTypes(availableProjectTypes => availableProjectTypes.map(([mappedType, active]) => projectType === mappedType ? [mappedType, false] : [mappedType, active]));
    setFiles((files) => [...files, { projectType: projectType } as SeeFile]);
  }

  function removeFile(idx: number) {
    const projectType = files[idx].projectType;
    setFiles((files) => files.filter((_, itemIdx) => itemIdx !== idx));
    setAvailableProjectTypes(availableProjectTypes => availableProjectTypes.map(([mappedType, active]) => projectType === mappedType ? [mappedType, true] : [mappedType, active]));
  }

  async function createServer() {
    const createServerResponse = await axiosInstance.post("/server/create", { name: name, avatarSeed: avatarSeed, avatarColor: avatarColor }).catch((error) => {
      AppUtils.notifyAxiosError(error, "Error Creating Server");
      return;
    });
    if (!createServerResponse) { return; }

    let success = true;
    for (let i = 0; i < files.length; i++) {
      if (!files[i]._localfile) {
        continue;
      }
      const projectFile: SeeFile = files[i];
      const form = new FormData();
      form.append("id", createServerResponse.data.id);
      form.append("projectType", projectFile.projectType);
      form.append("file", projectFile._localfile);
      await axiosInstance.post("/server/addFile", form).catch((error) => {
        success = false;
        AppUtils.notifyAxiosError(error, "Error Uploading File");
      });
    }

    if (success) {
      navigate('/server#' + createServerResponse.data.id, { replace: true });
    }
  }

  return (
    <Container sx={{ padding: "3em" }}>
      <Modal
        open={fileTypeModalOpen}
        onClose={() => setFileTypeModalOpen(false)}
        aria-labelledby="remove-user-modal-title"
        aria-describedby="remove-user-modal-description">
        <Box sx={modalStyle}>
          <Typography id="modal-title" variant="h6" sx={{ marginBottom: "6pt" }}>
            Select Code City Type
          </Typography>
          <Stack direction="column" spacing={1}>
            {availableProjectTypes.filter(([, active]) => active === true).map(([projectType]) =>
              <Button key={projectType} variant="contained" color="primary" sx={{ borderRadius: "25px", width: "100%" }} onClick={() => { setFileTypeModalOpen(false); addFile(projectType); }}>
                {ProjectTypeUtils.getLabel(projectType)}
              </Button>
            )}
          </Stack>
          <Stack justifyContent="end" direction="row" spacing={2} sx={{ marginTop: "2em" }}>
            <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => setFileTypeModalOpen(false)}>
              Cancel
            </Button>
          </Stack>
        </Box>
      </Modal>

      <Header />
      <Typography variant="h4">
        <Box display={"inline"} sx={{ "&:hover": { cursor: "pointer" } }}>
          <FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)} />&nbsp;
        </Box>
        New Game Server
      </Typography>
      <Card sx={{ marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto" }}>
        <CardContent sx={{ height: "calc(100% - 3em)" }}>
          <Stack direction="column" spacing={2}>
            <Stack direction="row" spacing={2}>
              <Stack direction="column" spacing={2} width={"100%"}>
                <Stack direction="column" flexGrow={1}>
                  <Typography variant="h6">Details</Typography>
                  <TextField error={!!errors.get("name")} helperText={errors.get("name")} value={name} onChange={(e) => setName(e.target.value)} label="Server Name" variant="standard" autoFocus />
                </Stack>

                <Typography variant="h6">Visualizations</Typography>
                <Card sx={{ borderRadius: "0px", flexGrow: 1, overflow: "auto", minHeight: "100px" }} elevation={0}>
                  <Stack direction="column" spacing={1}>
                    {files.map((projectFile, idx) =>
                      <Stack key={idx} direction="row">
                        <MuiFileInput
                          label={ProjectTypeUtils.getLabel(projectFile.projectType)}
                          placeholder="Click to choose file…"
                          variant="standard"
                          fullWidth
                          value={projectFile._localfile}
                          onChange={(value) => { setFiles((files) => files.map((item, itemIdx) => itemIdx === idx ? { ...item, _localfile: value } as SeeFile : item)) }}
                          clearIconButtonProps={{ title: "Remove", children: <FontAwesomeIcon icon={faX} /> }}
                          inputProps={{ accept: ProjectTypeUtils.getFileExtension(projectFile.projectType) }} />
                        <IconButton
                          size="small"
                          onClick={() => removeFile(idx)}>
                          <FontAwesomeIcon icon={faRemove} />
                        </IconButton>
                      </Stack>
                    )
                    }
                  </Stack>
                  <Button variant="contained" color="primary" sx={{ borderRadius: "25px", marginTop: "6pt" }} onClick={() => setFileTypeModalOpen(true)}>
                    Add Code City
                  </Button>
                </Card>
              </Stack>

              {/* Avatar */}
              <Box width={140} height={140}>
                <Card sx={{ width: "100%", height: "100%" }}>
                  <CardActionArea onMouseEnter={() => setDisplayReloadIcon(true)} onMouseLeave={() => setDisplayReloadIcon(false)} onClick={() => { setAvatarSeed(getRandomSeed()); setAvatarColor(getRandomColor()); }}>
                    <Box
                      visibility={displayReloadIcon ? "visible" : "hidden"}
                      position="absolute" color={grey[500]}
                      display="flex"
                      width={140}
                      height={140}
                      justifyContent="center"
                      sx={{ mixBlendMode: "difference" }}>
                      <Stack direction="column" justifyContent="center">
                        <FontAwesomeIcon icon={faRepeat} size="4x" />
                      </Stack>
                    </Box>
                    <Avatar width={140} height={140} avatarSeed={avatarSeed} avatarColor={avatarColor} />
                  </CardActionArea>
                </Card>
              </Box>
            </Stack>

            <Stack justifyContent="end" direction="row" spacing={2}>
              <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => navigate('/')}>
                Cancel
              </Button>
              <Button
                variant="contained"
                sx={{ borderRadius: "25px" }}
                onClick={() => {
                  setErrors(new Map<string, string>());
                  const tempErrorsList = new Map<string, string>();
                  if (!name) {
                    tempErrorsList.set('name', "Please enter a name for the server.");
                  }
                  if (tempErrorsList.size > 0) {
                    setErrors(tempErrorsList);
                  } else {
                    createServer()
                  }
                }}>
                Create
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Container>
  )
}

export default CreateServerView;
