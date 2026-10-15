class Libyaml < Formula
  desc "YAML parser and emitter library"
  homepage "https://yaml.org/"
  version "0.2.5"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libyaml-0.2.5-2-any.pkg.tar.zst"
  sha256 "bf4c28a3a92a46db3c809b0789aef731ae9a0c4283db368c7e8624550fed07f3"
  license "MIT"

  # UCRT64 development library; headers/import libraries are installed in the keg include/lib directories.

  def install
    install_tree
    install_library
  end
end
