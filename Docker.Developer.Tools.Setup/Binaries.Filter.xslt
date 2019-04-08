<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <!--Define output as indented xml-->
  <xsl:output method="xml" indent="yes" />

  <!--Remove empty lines-->
  <xsl:strip-space  elements="*" />

  <!--Add Common.wxi include to the result-->
  <xsl:template match="wix:Wix">
    <xsl:copy>
      <xsl:processing-instruction name="include">Common.wxi</xsl:processing-instruction>
      <!--Debugging-->
      <!--<te></te>-->
      <!--<xsl:for-each select="//wix:Directory[@Name = 'de']/wix:Component">
          <xsl:copy-of select="."/>   
        </xsl:for-each>-->
      <xsl:apply-templates />
    </xsl:copy>
  </xsl:template>

  <!--Remove non binary(dll) files-->
  <!--Create key for matching all components where source does not end with 'dll'-->
  <xsl:key name="nonBinary-component-id" match="//wix:Component[substring(wix:File/@Source, string-length(wix:File/@Source) - 2) != 'dll']" use="@Id" />
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>
  <!--Remove all components where id match the 'nonBinary-component-id' key-->
  <xsl:template match="wix:Component[@Id = key('nonBinary-component-id', @Id)/@Id]" />
  <!--Remove all component ref's where id match the 'nonBinary-component-id' key-->
  <xsl:template match="wix:ComponentRef[@Id = key('nonBinary-component-id', @Id)/@Id]" />

  <!--Remove all non 'da' subfolders (including files)-->
  <!--Create key for matching all components where parent node name is not 'da'-->
  <xsl:key name="nonDa-directory-child-id" match="//wix:Directory[@Name != 'da']/wix:Component" use="@Id" />
  <!--Remove all component ref's where id match the 'nonDa-directory-child-id' key-->
  <xsl:template match="wix:ComponentRef[@Id = key('nonDa-directory-child-id', @Id)/@Id]" />
  <!--Remove all Directories where name is not 'da'-->
  <xsl:template match="wix:Directory[@Name != 'da']" />
</xsl:stylesheet>