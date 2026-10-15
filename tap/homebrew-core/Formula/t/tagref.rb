class Tagref < Formula
  desc "Refer to other locations in your codebase"
  homepage "https://github.com/stepchowfun/tagref"
  url "https://github.com/stepchowfun/tagref/releases/download/v1.14.0/tagref-x86_64-pc-windows-msvc.exe"
  sha256 "df2c395fe092a1c8806bb20fbc6dba16e6e737502e14d340152d2a1aed69994d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stepchowfun/tagref/archive/refs/tags/v1.14.0.tar.gz"
  end

  def install
    bin.install "tagref-x86_64-pc-windows-msvc.exe" => "tagref.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tagref --version 2>&1")
  end
end
