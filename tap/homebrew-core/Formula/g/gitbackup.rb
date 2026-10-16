class Gitbackup < Formula
  desc "Tool to backup your Bitbucket, GitHub and GitLab repositories"
  homepage "https://github.com/amitsaha/gitbackup"
  url "https://github.com/amitsaha/gitbackup/releases/download/v1.3.1/gitbackup_Windows_x86_64.tar.gz"
  sha256 "f710b2b62743862857d3c0adc67b6e311d47b0f87380b930b506f89df8b32174"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/amitsaha/gitbackup/archive/refs/tags/v1.3.1.tar.gz"
  end

  def install
    bin.install "gitbackup.exe"
  end

  test do
    assert_match "please specify the git service type", shell_output("#{bin}/gitbackup 2>&1", 1)
  end
end
