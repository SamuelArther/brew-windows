class InstallNothing < Formula
  desc "Simulates installing things but doesn't actually install anything"
  homepage "https://github.com/buyukakyuz/install-nothing"
  url "https://github.com/buyukakyuz/install-nothing/releases/download/v0.5.0/install-nothing-windows-x86_64.exe"
  sha256 "312ff862e1fa405eaa576bd49b0cd3ab8b5a58234fe57b8e36c909c40fc8c229"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/buyukakyuz/install-nothing/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "install-nothing-windows-x86_64.exe" => "install-nothing.exe"
  end

  test do
    system bin/"install-nothing", "--version"
  end
end
