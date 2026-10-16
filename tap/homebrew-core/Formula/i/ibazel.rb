class Ibazel < Formula
  desc "Tools for building Bazel targets when source files change"
  homepage "https://github.com/bazelbuild/bazel-watcher"
  url "https://github.com/bazelbuild/bazel-watcher/releases/download/v0.33.0/ibazel_windows_amd64.exe"
  sha256 "5be92e134bc3ee255e1918ce96ea161fcc3b308e8e79b660ab5f9b0900c178ae"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bazelbuild/bazel-watcher/archive/refs/tags/v0.33.0.tar.gz"
  end

  def install
    bin.install "ibazel_windows_amd64.exe" => "ibazel.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ibazel version 2>&1")
  end
end
