class LsLint < Formula
  desc "Extremely fast file and directory name linter"
  homepage "https://ls-lint.org/"
  url "https://github.com/loeffel-io/ls-lint/releases/download/v2.3.1/ls-lint-windows-amd64.tar.gz"
  sha256 "b65c4c168c72cb1d82f0c5a4f2717238d4653bf275c37c7aa9d3d9df376fd19d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/loeffel-io/ls-lint/archive/refs/tags/v2.3.1.tar.gz"
  end

  def install
    bin.install "ls-lint-windows-amd64.exe" => "ls-lint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ls-lint --version 2>&1")
  end
end
