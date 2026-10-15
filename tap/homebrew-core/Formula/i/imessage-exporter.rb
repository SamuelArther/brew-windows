class ImessageExporter < Formula
  desc "Command-line tool to export and inspect local iMessage database"
  homepage "https://github.com/ReagentX/imessage-exporter"
  url "https://github.com/ReagentX/imessage-exporter/releases/download/4.3.0/imessage-exporter-x86_64-pc-windows-gnu.tar.gz"
  sha256 "2a3850b25cae6c064ed245a932f36f8c899118431685391b3bd21314586400e0"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ReagentX/imessage-exporter/archive/refs/tags/4.3.0.tar.gz"
  end

  def install
    bin.install "imessage-exporter.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/imessage-exporter --version")
    output = shell_output("#{bin}/imessage-exporter --diagnostics 2>&1", 1)
    assert_match "Invalid configuration", output
  end
end
