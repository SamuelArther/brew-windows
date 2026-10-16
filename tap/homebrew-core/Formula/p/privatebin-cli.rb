class PrivatebinCli < Formula
  desc "CLI for creating and managing PrivateBin pastes"
  homepage "https://github.com/gearnode/privatebin"
  url "https://github.com/gearnode/privatebin/releases/download/v2.2.1/privatebin_Windows_x86_64.tar.gz"
  sha256 "9bfc7677c42ed06d1e7d3564fcf64b1186a2dcca9a8488bb5c985d08abeb4bc8"
  license "ISC"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gearnode/privatebin/releases/download/v2.2.1/v2.2.1-source.tar.gz"
  end

  def install
    bin.install "privatebin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/privatebin --version")

    assert_match "Error: no privatebin instance configured", shell_output("#{bin}/privatebin create foo 2>&1", 1)
  end
end
