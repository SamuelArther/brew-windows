class Tinysvm < Formula
  desc "Support vector machine library for pattern recognition"
  homepage "http://chasen.org/~taku/software/TinySVM/"
  url "https://cdn.netbsd.org/pub/pkgsrc/distfiles/TinySVM-0.09.tar.gz"
  mirror "http://chasen.org/~taku/software/TinySVM/src/TinySVM-0.09.tar.gz"
  sha256 "e377f7ede3e022247da31774a4f75f3595ce768bc1afe3de9fc8e962242c7ab8"
  license "LGPL-2.1-or-later"
  compatibility_version 1

  livecheck do
    url :homepage
    regex(/href=.*?TinySVM[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "6229c17452d388c4c2e1f2c39a2c10b19e3e8ed52c11bcee87610dbc176451a9"
  end

  # Use correct compilation flag, via MacPorts.
  patch :p0 do
    file "Patches/tinysvm/patch-configure.diff"
    type :unofficial
  end

  def install
    # Needed to select proper getopt, per MacPorts
    ENV.append_to_cflags "-D__GNU_LIBRARY__"

    # Fix for newer clang
    ENV.append_to_cflags "-Wno-implicit-int" if DevelopmentTools.clang_build_version >= 1403

    args = []
    # Help old config scripts identify arm64 linux
    args << "--host=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", "--mandir=#{man}", "--disable-shared", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"train.svmdata").write <<~EOS
      +1 201:1.2 3148:1.8 3983:1 4882:1
      -1 874:0.3 3652:1.1 3963:1 6179:1
      +1 1168:1.2 3318:1.2 3938:1.8 4481:1
      +1 350:1 3082:1.5 3965:1 6122:0.2
      -1 99:1 3057:1 3957:1 5838:0.3
    EOS

    (testpath/"train.svrdata").write <<~EOS
      0.23 201:1.2 3148:1.8 3983:1 4882:1
      0.33 874:0.3 3652:1.1 3963:1 6179:1
      -0.12 1168:1.2 3318:1.2 3938:1.8 4481:1
    EOS

    system bin/"svm_learn", "-t", "1", "-d", "2", "-c", "train.svmdata", "test"
    system bin/"svm_classify", "-V", "train.svmdata", "test"
    system bin/"svm_model", "test"

    assert_path_exists testpath/"test"
  end
end
