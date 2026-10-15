class Heksa < Formula
  desc "CLI hex dumper with colors"
  homepage "https://github.com/raspi/heksa"
  url "https://github.com/raspi/heksa/releases/download/v1.14.0/heksa-v1.14.0-windows-amd64.zip"
  sha256 "f15c301ca469950388aa0130b99c7ef521c90c9c4e171219ffb2cb5277e0c78c"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/raspi/heksa.git"
  end

  def install
    install_tree
    windows_exe "libexec/bin/heksa.exe", "heksa"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/heksa --version 2>&1")
  end
end
