class TodoistCliGo < Formula
  desc "CLI for Todoist"
  homepage "https://github.com/sachaos/todoist"
  url "https://github.com/sachaos/todoist/releases/download/v0.24.0/todoist_Windows_x86_64.tar.gz"
  sha256 "3482b55c654757661c831719d651ee05cae208fe18553cc3b6e7c639d113078f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sachaos/todoist/archive/refs/tags/v0.24.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/todoist.exe", "todoist"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/todoist --version")

    test_config = testpath/".config/todoist/config.json"
    test_config.write <<~JSON
      {
        "token": "test_token"
      }
    JSON
    chmod 0600, test_config

    output = shell_output("#{bin}/todoist list 2>&1")
    assert_match "There is no task.", output
  end
end
