import { AppBar, Box, Container, IconButton, Toolbar } from "@mui/material";
import seeLogo from "../img/see-logo.png";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCog, faRightFromBracket, faUser } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from "react-router";
import { useContext } from "react";
import { AuthContext } from "../contexts/AuthContext";
import AppUtils from "../utils/AppUtils";

function Header() {
  const { axiosInstance, user, setUser } = useContext(AuthContext);

  async function logout() {
    await axiosInstance.post("/user/signout").then(
      () => {
        setUser(null);
        sessionStorage.setItem("username", "");
      }
    ).catch(
      (error) => AppUtils.notifyAxiosError(error, "Error During Sign-Out")
    );

  }

  const navigate = useNavigate();

  return (
    <AppBar position="relative" color="transparent" elevation={0}>
      <Container disableGutters>
        <Toolbar disableGutters>
          <Box sx={{ flexGrow: 1 }}>
            <a href="/">
              <img src={seeLogo} height="64" alt="SEE Logo" />
            </a>
          </Box>
          {
            user?.roles.some((item) => item.name == "ROLE_ADMIN") ?
              <IconButton size="large" onClick={() => navigate('/settings')}>
                <FontAwesomeIcon icon={faCog} />
              </IconButton>
              : <></>
          }
          <IconButton size="large" onClick={() => navigate('/userSettings')}>
            <FontAwesomeIcon icon={faUser} />
          </IconButton>
          <IconButton size="large" onClick={() => logout()}>
            <FontAwesomeIcon icon={faRightFromBracket} />
          </IconButton>
        </Toolbar>
      </Container>
    </AppBar>
  )
}

export default Header;
