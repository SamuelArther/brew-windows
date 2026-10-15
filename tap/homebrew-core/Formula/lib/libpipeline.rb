class Libpipeline < Formula
  desc "C library for manipulating pipelines of subprocesses"
  homepage "https://libpipeline.gitlab.io/libpipeline/"
  url "https://repo.msys2.org/msys/x86_64/libpipeline-1.5.8-1-x86_64.pkg.tar.zst"
  sha256 "3a872f7f37303409f1b546964ac146dffb9b33e92e42e0219b61157e69830e28"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.savannah.nongnu.org/releases/libpipeline/"
    regex(/href=.*?libpipeline[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"msys-pipeline-1.dll"
  end
end
