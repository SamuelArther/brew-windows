class Gensio < Formula
  desc "Stream I/O Library"
  homepage "https://github.com/cminyard/gensio"
  url "https://github.com/cminyard/gensio/releases/download/v3.0.4/Gensio-3.0.4-windows.exe"
  sha256 "414a9db565ddff45af088b1729f4f539192afab3ec5eb318dc2db9e39e240115"
  license all_of: ["LGPL-2.1-only", "GPL-2.0-only", "Apache-2.0"]
  revision 2

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cminyard/gensio/releases/download/v3.0.4/gensio-3.0.4.tar.gz"
  end

  def install
    bin.install "bin/gensiot.exe" => "gagwpe.exe"
  end

  test do
    assert_path_exists bin/"gagwpe.exe"
  end
end
