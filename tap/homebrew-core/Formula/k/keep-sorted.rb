class KeepSorted < Formula
  desc "Language-agnostic formatter that sorts selected lines"
  homepage "https://github.com/google/keep-sorted"
  url "https://github.com/google/keep-sorted/releases/download/v0.10.0/keep-sorted_windows_amd64.exe"
  sha256 "adbbea4b3eac632fc5da1053b31377f74516a43fb3b00c325dbe37f3fc311e45"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/keep-sorted.git"
  end

  def install
    bin.install "keep-sorted_windows_amd64.exe" => "keep-sorted.exe"
  end

  test do
    system bin/"keep-sorted", "--version"
  end
end
