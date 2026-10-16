class Resticprofile < Formula
  desc "Configuration profiles manager and scheduler for restic backup"
  homepage "https://creativeprojects.github.io/resticprofile/"
  url "https://github.com/creativeprojects/resticprofile/releases/download/v0.33.1/resticprofile_0.33.1_windows_amd64.tar.gz"
  sha256 "54baefff78cd217b4033345d785d559ed473dd0d809c32e676a8005268fd8aec"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/creativeprojects/resticprofile/archive/refs/tags/v0.33.1.tar.gz"
  end

  def install
    bin.install "resticprofile.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/resticprofile version 2>&1")
  end
end
