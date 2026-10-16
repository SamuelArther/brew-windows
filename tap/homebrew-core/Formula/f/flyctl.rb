class Flyctl < Formula
  desc "Command-line tools for fly.io services"
  homepage "https://fly.io"
  url "https://github.com/superfly/flyctl/releases/download/v0.4.118/flyctl_0.4.118_Windows_x86_64.zip"
  sha256 "a630dce804b7a2b9dcee2bdf150b01792ab381982ed3c9820f1e1ffeb481bae2"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/flyctl.exe", "flyctl"
    windows_exe "libexec/flyctl.exe", "fly"
  end

  test do
    # Windows builds name themselves flyctl.exe
    assert_match(/flyctl(\.exe)? v#{Regexp.escape(version.to_s)}/, shell_output("#{bin}/flyctl version"))
  end
end
