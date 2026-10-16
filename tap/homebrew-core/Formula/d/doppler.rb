class Doppler < Formula
  desc "CLI for interacting with Doppler secrets and configuration"
  homepage "https://docs.doppler.com/docs"
  url "https://github.com/DopplerHQ/cli/releases/download/3.77.0/doppler_3.77.0_windows_amd64.zip"
  sha256 "5f4f7bfade468d77706acf365c6e5045afd234031743b15bd07af034857f46fe"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/DopplerHQ/cli/archive/refs/tags/3.77.0.tar.gz"
  end

  def install
    bin.install "doppler.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/doppler --version")

    output = shell_output("#{bin}/doppler setup 2>&1", 1)
    assert_match "Doppler Error: you must provide a token", output
  end
end
