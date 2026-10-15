class Austin < Formula
  desc "Python frame stack sampler for CPython"
  homepage "https://github.com/P403n1x87/austin"
  url "https://github.com/P403n1x87/austin/releases/download/v4.0.0/austin-4.0.0-win64.zip"
  sha256 "ae07dfe945eea1e816c0df57da1f02fe0b8d79f2acb45be680772f655e9187fb"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/P403n1x87/austin/archive/refs/tags/v4.0.0.tar.gz"
  end

  def install
    bin.install "austin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/austin --version 2>&1")
  end
end
