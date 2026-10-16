class WakatimeCli < Formula
  desc "Command-line interface to the WakaTime api"
  homepage "https://wakatime.com/"
  url "https://github.com/wakatime/wakatime-cli/releases/download/v2.27.5/wakatime-cli-windows-amd64.zip"
  sha256 "ed8c399e7778e4adbc9d67db89dc0d972a5d3e9f0f27ea669557905f60bba506"
  license "BSD-3-Clause"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wakatime/wakatime-cli.git"
  end

  def install
    bin.install "wakatime-cli-windows-amd64.exe" => "wakatime-cli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wakatime-cli --version 2>&1")
  end
end
