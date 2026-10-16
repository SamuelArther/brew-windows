class Cheat < Formula
  desc "Create and view interactive cheat sheets for *nix commands"
  homepage "https://github.com/cheat/cheat"
  url "https://github.com/cheat/cheat/releases/download/5.1.0/cheat-windows-amd64.exe.zip"
  sha256 "a7fbdb2c02d575c2d75aa350ac25006699586edccbfce2304a01b5f518e06b80"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cheat/cheat/archive/refs/tags/5.1.0.tar.gz"
  end

  def install
    bin.install "cheat-windows-amd64.exe" => "cheat.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cheat --version 2>&1")
  end
end
