class ExcalidrawConverter < Formula
  desc "Command-line tool for porting Excalidraw diagrams to Gliffy"
  homepage "https://github.com/sindrel/excalidraw-converter"
  url "https://github.com/sindrel/excalidraw-converter/releases/download/v1.5.6/excalidraw-converter_1.5.6_windows_amd64.zip"
  sha256 "1862968be1d0a598065ffe769b9bc0983937a221069c165684ca2be45a666346"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sindrel/excalidraw-converter/archive/refs/tags/v1.5.6.tar.gz"
  end

  def install
    bin.install "exconv.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/exconv version 2>&1")
  end
end
