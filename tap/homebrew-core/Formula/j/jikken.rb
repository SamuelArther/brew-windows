class Jikken < Formula
  desc "Powerful, source control friendly REST API testing toolkit"
  homepage "https://jikken.io/"
  url "https://github.com/jikkenio/jikken/releases/download/v0.8.4/jikken-v0.8.4-x86_64-windows.zip"
  sha256 "2e28eb78172059df7ff77166696489e4da1fa27633cd154d1f268b16228354a4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jikkenio/jikken/archive/refs/tags/v0.8.4.tar.gz"
  end

  def install
    bin.install "jk.exe"
  end

  test do
    output = shell_output("#{bin}/jk new test")
    assert_match "Successfully created test (`test.jkt`).", output
    assert_match "status: 200", (testpath/"test.jkt").read

    assert_match version.to_s, shell_output("#{bin}/jk --version")
  end
end
