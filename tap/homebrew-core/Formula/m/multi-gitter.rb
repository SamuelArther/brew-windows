class MultiGitter < Formula
  desc "Update multiple repositories in with one command"
  homepage "https://github.com/lindell/multi-gitter"
  url "https://github.com/lindell/multi-gitter/releases/download/v0.63.1/multi-gitter_0.63.1_Windows_x86_64.tar.gz"
  sha256 "b18fb79bb137b3e3c769a23eec74273f1f3a689fa5813060c887617958b3f2bf"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lindell/multi-gitter/archive/refs/tags/v0.63.1.tar.gz"
  end

  def install
    bin.install "multi-gitter.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/multi-gitter version")

    output = shell_output("#{bin}/multi-gitter status 2>&1", 1)
    assert_match "Error: no organization, user, repo, repo-search or code-search set", output
  end
end
