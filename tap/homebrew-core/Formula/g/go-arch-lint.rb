class GoArchLint < Formula
  desc "Architecture linter for Go projects"
  homepage "https://github.com/fe3dback/go-arch-lint"
  url "https://github.com/fe3dback/go-arch-lint/releases/download/v1.19.0/go-arch-lint_1.19.0_windows_amd64.zip"
  sha256 "f73aaefeca181a0b39f70ae4bf8c0a4d099a0ab5814140c36930ffe181fb456c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fe3dback/go-arch-lint/archive/refs/tags/v1.19.0.tar.gz"
  end

  def install
    bin.install "go-arch-lint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/go-arch-lint version 2>&1")
  end
end
