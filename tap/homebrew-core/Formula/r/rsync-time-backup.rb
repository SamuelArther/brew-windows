class RsyncTimeBackup < Formula
  desc "Time Machine-style backup for the terminal using rsync"
  homepage "https://github.com/laurent22/rsync-time-backup"
  url "https://github.com/laurent22/rsync-time-backup/archive/refs/tags/v1.1.5.tar.gz"
  sha256 "567f42ddf2c365273252f15580bb64aa3b3a8abb4a375269aea9cf0278510657"
  license "MIT"
  head "https://github.com/laurent22/rsync-time-backup.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c3ccddf3376b5a90d964ddd03de93afdd33dce31276544041497ec26a39c471b"
  end

  def install
    bin.install "rsync_tmbackup.sh"
  end

  test do
    output = shell_output("#{bin}/rsync_tmbackup.sh --rsync-get-flags")
    assert_match "--times --recursive", output
  end
end
