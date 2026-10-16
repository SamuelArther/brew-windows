class Algolia < Formula
  desc "Command-line tool to manage Algolia applications, accounts, and search resources"
  homepage "https://www.algolia.com/doc/tools/cli/get-started"
  url "https://github.com/algolia/cli/releases/download/v1.17.1/algolia_1.17.1_windows_amd64.zip"
  sha256 "7f50736dee8fd966e3d51a5ca6159b92b916088eb16768729248daf1afacdddd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/algolia/cli/archive/refs/tags/v1.17.1.tar.gz"
  end

  def install
    bin.install "algolia.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/algolia --version")

    output = shell_output("#{bin}/algolia apikeys list 2>&1", 4)
    assert_match "you have not configured your Application ID yet", output
  end
end
