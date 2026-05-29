import { createRoot } from "react-dom/client";
import { ConvySummaryWidget } from "./ConvySummaryWidget.js";
import "./styles.css";

const root = document.getElementById("root");

if (root) {
  createRoot(root).render(<ConvySummaryWidget />);
}
