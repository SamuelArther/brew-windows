class LibvoAacenc < Formula
  desc "VisualOn AAC encoder library"
  homepage "https://opencore-amr.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/opencore-amr/vo-aacenc/vo-aacenc-0.1.3.tar.gz"
  sha256 "e51a7477a359f18df7c4f82d195dab4e14e7414cbd48cf79cc195fc446850f36"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(%r{url=.*?/vo-aacenc[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "59ab5377a68583df33c80726871b0636b535a4958303b84a6cf992b6b34de3a8"
  end

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <vo-aacenc/cmnMemory.h>

      int main()
      {
        VO_MEM_INFO info; info.Size = 1;
        VO_S32 uid = 0;
        VO_PTR pMem = (VO_PTR)cmnMemAlloc(uid, &info);
        cmnMemFree(uid, pMem);
        return 0;
      }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-lvo-aacenc", "-o", "test"
    system "./test"
  end
end
