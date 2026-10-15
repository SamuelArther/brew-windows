class Arelo < Formula
  desc "Simple auto reload (live reload) utility"
  homepage "https://github.com/makiuchi-d/arelo"
  url "https://github.com/makiuchi-d/arelo/releases/download/v1.15.4/arelo_Windows_x86_64.zip"
  sha256 "b63759eedc49a6514d8db006018cc1c0f36d04256a70e30e3ad19e7909fde26f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/makiuchi-d/arelo/archive/refs/tags/v1.15.4.tar.gz"
  end

  def install
    bin.install "arelo.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/arelo --version 2>&1")
  end
end
