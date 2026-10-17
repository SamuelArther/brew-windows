class Jasmin < Formula
  desc "Assembler for the Java Virtual Machine"
  homepage "https://jasmin.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/jasmin/jasmin/jasmin-2.4/jasmin-2.4.zip"
  sha256 "eaa10c68cec68206fd102e9ec7113739eccd790108a1b95a6e8c3e93f20e449d"
  license "BSD-4-Clause"
  revision 2

  bottle do
    sha256 cellar: :any, x86_64_windows: "41911fc41cdadaf14ae0a8ca8a6d5f3274c1247b88f1de1b5dcd499af4ecae4c"
  end

  depends_on "openjdk"

  def install
    # Remove Windows scripts
    rm_r(Dir["*.bat"])

    libexec.install Dir["*.jar"]
    prefix.install %w[Readme.txt license-ant.txt license-jasmin.txt]
    bin.write_jar_script libexec/"jasmin.jar", "jasmin"
  end

  test do
    (testpath/"test.j").write <<~EOS
      .class public HomebrewTest
      .super java/lang/Object
       .method public <init>()V
         aload_0
         invokespecial java/lang/Object/<init>()V
         return
      .end method
       .method public static main([Ljava/lang/String;)V
         .limit stack 2
         getstatic java/lang/System/out Ljava/io/PrintStream;
         ldc "Hello Homebrew"
         invokevirtual java/io/PrintStream/println(Ljava/lang/String;)V
         return
      .end method
    EOS
    system bin/"jasmin", testpath/"test.j"
    assert_equal "Hello Homebrew\n", shell_output("#{formula_opt_bin("openjdk")}/java HomebrewTest")
  end
end
