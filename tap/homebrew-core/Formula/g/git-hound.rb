class GitHound < Formula
  desc "Git plugin that prevents sensitive data from being committed"
  homepage "https://github.com/ezekg/git-hound"
  url "https://github.com/ezekg/git-hound/releases/download/1.0.0/git-hound_windows_amd64.exe"
  sha256 "09fbad18ab4e7ceb7f4ddd080126911ec1e68fc8f1e0c74d6f70f1aa5cb4f76a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ezekg/git-hound/archive/refs/tags/1.0.0.tar.gz"
  end

  def install
    bin.install "git-hound_windows_amd64.exe" => "git-hound.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-hound -v 2>&1")
  end
end
