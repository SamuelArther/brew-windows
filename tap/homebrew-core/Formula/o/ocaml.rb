class Ocaml < Formula
  desc "General purpose programming language in the ML family"
  homepage "https://ocaml.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ocaml-5.4.1-2-any.pkg.tar.zst"
  sha256 "bcf19ea8530c251bba819537cb2beb5953bdc38eb7e698a7a5eef0b8dbf60fd0"
  license "LGPL-2.1-only" => { with: "OCaml-LGPL-linking-exception" }

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ocaml.org/releases"
    regex(%r{href=.*?/releases/v?(\d+(?:\.\d+)+)/?["']}i)
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ocaml.exe", "ocaml"
    msys2_bin "ucrt64/bin/ocamlc.byte.exe", "ocamlc.byte"
    msys2_bin "ucrt64/bin/ocamlc.exe", "ocamlc"
    msys2_bin "ucrt64/bin/ocamlc.opt.exe", "ocamlc.opt"
    msys2_bin "ucrt64/bin/ocamlcmt.exe", "ocamlcmt"
    msys2_bin "ucrt64/bin/ocamlcp.exe", "ocamlcp"
    msys2_bin "ucrt64/bin/ocamldebug.exe", "ocamldebug"
    msys2_bin "ucrt64/bin/ocamldep.byte.exe", "ocamldep.byte"
    msys2_bin "ucrt64/bin/ocamldep.exe", "ocamldep"
    msys2_bin "ucrt64/bin/ocamldep.opt.exe", "ocamldep.opt"
    msys2_bin "ucrt64/bin/ocamldoc.exe", "ocamldoc"
    msys2_bin "ucrt64/bin/ocamldoc.opt.exe", "ocamldoc.opt"
    msys2_bin "ucrt64/bin/ocamllex.byte.exe", "ocamllex.byte"
    msys2_bin "ucrt64/bin/ocamllex.exe", "ocamllex"
    msys2_bin "ucrt64/bin/ocamllex.opt.exe", "ocamllex.opt"
    msys2_bin "ucrt64/bin/ocamlmklib.exe", "ocamlmklib"
    msys2_bin "ucrt64/bin/ocamlmktop.exe", "ocamlmktop"
    msys2_bin "ucrt64/bin/ocamlobjinfo.byte.exe", "ocamlobjinfo.byte"
    msys2_bin "ucrt64/bin/ocamlobjinfo.exe", "ocamlobjinfo"
    msys2_bin "ucrt64/bin/ocamlobjinfo.opt.exe", "ocamlobjinfo.opt"
    msys2_bin "ucrt64/bin/ocamlopt.byte.exe", "ocamlopt.byte"
    msys2_bin "ucrt64/bin/ocamlopt.exe", "ocamlopt"
    msys2_bin "ucrt64/bin/ocamlopt.opt.exe", "ocamlopt.opt"
    msys2_bin "ucrt64/bin/ocamloptp.exe", "ocamloptp"
    msys2_bin "ucrt64/bin/ocamlprof.exe", "ocamlprof"
    msys2_bin "ucrt64/bin/ocamlrun.exe", "ocamlrun"
    msys2_bin "ucrt64/bin/ocamlrund.exe", "ocamlrund"
    msys2_bin "ucrt64/bin/ocamlruni.exe", "ocamlruni"
    msys2_bin "ucrt64/bin/ocamlyacc.exe", "ocamlyacc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ocaml --version 2>&1")
  end
end
