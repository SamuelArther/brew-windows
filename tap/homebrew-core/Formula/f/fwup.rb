class Fwup < Formula
  desc "Configurable embedded Linux firmware update creator and runner"
  homepage "https://github.com/fwup-home/fwup"
  url "https://github.com/fwup-home/fwup/releases/download/v1.17.1/fwup-1.17.1-windows-x86_64.exe"
  sha256 "f8c46e207f32d71caf40022362ec101d0b36150e7dffd35f47acd394e13c3d66"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fwup-home/fwup/releases/download/v1.17.1/fwup-1.17.1.tar.gz"
  end

  def install
    bin.install "fwup-1.17.1-windows-x86_64.exe" => "fwup.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fwup --version 2>&1")
  end
end
