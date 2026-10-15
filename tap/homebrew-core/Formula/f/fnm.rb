class Fnm < Formula
  desc "Fast and simple Node.js version manager"
  homepage "https://github.com/Schniz/fnm"
  url "https://github.com/Schniz/fnm/releases/download/v1.39.0/fnm-windows.zip"
  sha256 "8183bed4348cb78fdfd8abb3d1247fbeab7b2082f941363929c61e747c001e10"
  license "GPL-3.0-only"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "fnm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fnm --version")
  end
end
