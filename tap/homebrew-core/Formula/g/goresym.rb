class Goresym < Formula
  desc "Go symbol recovery tool"
  homepage "https://github.com/mandiant/GoReSym"
  url "https://github.com/mandiant/GoReSym/releases/download/v3.4.1/GoReSym-windows.zip"
  sha256 "c051f182412197d834bd3cbd91424831b7aadb85dfe6548603873672560a8dea"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mandiant/GoReSym/archive/refs/tags/v3.4.1.tar.gz"
  end

  def install
    bin.install "GoReSym.exe" => "goresym.exe"
  end

  test do
    system bin/"goresym", "--help"
  end
end
