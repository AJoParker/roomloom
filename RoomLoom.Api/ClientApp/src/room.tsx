import { createRoot } from "react-dom/client";

// Pipeline-proving stub. Saturday replaces this with the LiveKit
// <VideoConference /> island per SCOPE.md closed decisions.
function IslandScaffold() {
  return <p data-island="ok">React island scaffold OK.</p>;
}

const host = document.getElementById("livekit-root");
if (host) {
  createRoot(host).render(<IslandScaffold />);
} else {
  console.info("[room.js] no #livekit-root on this page; island not mounted.");
}
