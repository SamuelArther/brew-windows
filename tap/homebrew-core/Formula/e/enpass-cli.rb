class EnpassCli < Formula
  desc "Enpass command-line client"
  homepage "https://github.com/hazcod/enpass-cli"
  url "https://github.com/hazcod/enpass-cli/releases/download/v1.14.0/enpass-cli_1.14.0_windows_amd64.zip"
  sha256 "5121375610264dc44fd13e5762b9f5e1ef30980823b883411f97c1df6c4c3ba2"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hazcod/enpass-cli/archive/refs/tags/v1.14.0.tar.gz"
  end

  def install
    bin.install "enpasscli.exe" => "enpass-cli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/enpass-cli version 2>&1")
  end
end
