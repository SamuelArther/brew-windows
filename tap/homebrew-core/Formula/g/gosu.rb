class Gosu < Formula
  desc "Pragmatic language for the JVM"
  homepage "https://gosu-lang.github.io/"
  url "https://github.com/gosu-lang/gosu-lang/archive/refs/tags/v1.18.13.tar.gz"
  sha256 "b6d6f83270c6badea7baa882e2658399879fde3e4d695b93685d34853f30d1de"
  license "Apache-2.0"
  head "https://github.com/gosu-lang/gosu-lang.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "36d77b07f0bb434b04e2c4cb592cf9889df5936b81147687dd392b5f86882f3c"
  end

  depends_on "maven" => :build
  depends_on "openjdk@17"

  skip_clean "libexec/ext"

  # Drop gosu-doc (javadoc internals don't compile on JDK 17+) and uncomment
  # JDK 13+ TreeVisitor stubs upstream left disabled.
  patch :DATA

  def install
    ENV["JAVA_HOME"] = Language::Java.java_home("17")

    system "mvn", "package"
    libexec.install Dir["gosu/target/gosu-#{version}-full/gosu-#{version}/*"]
    (libexec/"ext").mkpath
    (bin/"gosu").write_env_script libexec/"bin/gosu", Language::Java.java_home_env("17")
  end

  test do
    (testpath/"test.gsp").write 'print ("burp")'
    assert_equal "burp", shell_output("#{bin}/gosu test.gsp").chomp
  end
end

__END__
--- a/pom.xml
+++ b/pom.xml
@@ -25,7 +25,6 @@
     <module>gosu-core-api-precompiled</module>
     <module>gosu-process</module>
     <module>gosu-lab</module>
-    <module>gosu-doc</module>
     <module>gosu-maven-compiler</module>
     <module>gosu-parent</module>
     <module>gosu-test</module>
--- a/gosu/pom.xml
+++ b/gosu/pom.xml
@@ -35,12 +35,6 @@
       <version>${project.version}</version>
       <scope>runtime</scope>
     </dependency>
-    <dependency>
-      <groupId>org.gosu-lang.gosu</groupId>
-      <artifactId>gosu-doc</artifactId>
-      <version>${project.version}</version>
-      <scope>runtime</scope>
-    </dependency>
   </dependencies>

   <build>
--- a/gosu-lab/src/main/java/editor/util/transform/java/visitor/GosuVisitor.java
+++ b/gosu-lab/src/main/java/editor/util/transform/java/visitor/GosuVisitor.java
@@ -2210,35 +2210,35 @@

   // Overrides for visitors new in Java 17...

-//  public String visitBindingPattern( BindingPatternTree node, Object o )
-//  {
-//    return null;
-//  }
+  public String visitBindingPattern( BindingPatternTree node, Object o )
+  {
+    return null;
+  }
 //
-//  public String visitDefaultCaseLabel( DefaultCaseLabelTree node, Object o )
-//  {
-//    return null;
-//  }
+  public String visitDefaultCaseLabel( DefaultCaseLabelTree node, Object o )
+  {
+    return null;
+  }
 //
-//  public String visitGuardedPattern( GuardedPatternTree node, Object o )
-//  {
-//    return null;
-//  }
-//
-//  public String visitParenthesizedPattern( ParenthesizedPatternTree node, Object o )
-//  {
-//    return null;
-//  }
+  public String visitGuardedPattern( GuardedPatternTree node, Object o )
+  {
+    return null;
+  }
 //
-//  public String visitSwitchExpression( SwitchExpressionTree node, Object o )
-//  {
-//    return null;
-//  }
+  public String visitParenthesizedPattern( ParenthesizedPatternTree node, Object o )
+  {
+    return null;
+  }
 //
-//  public String visitYield( YieldTree node, Object o )
-//  {
-//    return null;
-//  }
+  public String visitSwitchExpression( SwitchExpressionTree node, Object o )
+  {
+    return null;
+  }
+//
+  public String visitYield( YieldTree node, Object o )
+  {
+    return null;
+  }

   private void pushIndent()
   {
