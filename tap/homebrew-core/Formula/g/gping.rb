class Gping < Formula
  desc "Ping, but with a graph"
  homepage "https://github.com/orf/gping"
  url "https://github.com/orf/gping/releases/download/gping-v1.21.0/gping-Windows-msvc-x86_64.zip"
  version "1.21.0"
  sha256 "0de92fab46f4de8ac63f6d3247e9426bf430fae08d231bb513987dc7ea5b331d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "gping.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gping --version")
  end
end
