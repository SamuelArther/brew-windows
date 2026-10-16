class Talisman < Formula
  desc "Tool to detect and prevent secrets from getting checked in"
  homepage "https://thoughtworks.github.io/talisman/"
  url "https://github.com/thoughtworks/talisman/releases/download/v1.37.0/talisman_windows_amd64.exe"
  sha256 "64b4f61fadd254f0bf32b5411e678564a8f201bd61a811105e286495df644be1"
  license "MIT"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/thoughtworks/talisman/archive/refs/tags/v1.37.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "talisman_windows_amd64.exe" => "talisman.exe"
  end

  test do
    system "git", "init", "."
    assert_match "talisman scan report", shell_output("#{bin}/talisman --scan")
  end
end
