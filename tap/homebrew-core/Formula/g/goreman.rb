class Goreman < Formula
  desc "Foreman clone written in Go"
  homepage "https://github.com/mattn/goreman"
  url "https://github.com/mattn/goreman/releases/download/v0.3.19/goreman_v0.3.19_windows_amd64.zip"
  sha256 "59efb8a30769e40cc597f2c3bfebf364750acc142396ab9e9e763840a3ab3052"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url :homepage
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "goreman.exe"
  end

  test do
    (testpath/"Procfile").write "web: echo 'hello' > goreman-homebrew-test.out"
    system bin/"goreman", "start"
    assert_path_exists testpath/"goreman-homebrew-test.out"
    assert_match "hello", (testpath/"goreman-homebrew-test.out").read
  end
end
