pushd "..\Photon Server\Photon-OnPremise-Server-SDK_v4-0-29-11263\deploy\bin_Win64"
start /wait PhotonSocketServer /stop
start PhotonSocketServer /run LoadBalancing
popd