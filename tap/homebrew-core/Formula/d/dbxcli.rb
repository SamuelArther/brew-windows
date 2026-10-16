class Dbxcli < Formula
  desc "Command-line tool for Dropbox users and team admins"
  homepage "https://github.com/dropbox/dbxcli"
  url "https://github.com/dropbox/dbxcli/releases/download/v3.7.4/dbxcli_3.7.4_windows_amd64.zip"
  sha256 "a772a6c83be56fd6bb07ab9b2a84dcbb94c472bd8dd02b22309c55095afd6cf8"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dropbox/dbxcli/archive/refs/tags/v3.7.4.tar.gz"
  end

  def install
    bin.install "dbxcli.exe"
  end

  test do
    ENV["DBXCLI_AUTH_FILE"] = testpath/"missing-auth.json"
    output = shell_output("#{bin}/dbxcli ls 2>&1", 2)
    assert_match "no saved Dropbox credentials", output
  end
end
